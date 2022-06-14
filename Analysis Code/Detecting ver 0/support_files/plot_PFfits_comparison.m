function  plot_PFfits_comparison(dataIN, cfg)
% helper function to plot the psychometric fits for standing, walking, and
% walking (gait) quantiles.

% called from the script
% plotjobs_Data_withinGait_PFfits.m

GFX_headY = cfg.HeadData;
usecolsStWlk = {[.7 .7 .7], [.7 .7 0]}; % R Gr
usecolsLR= {[.7 0, 0], [0 .7 0], [.7, 0, .7]}; % R Gr Prple.
figure(1); clf; set(gcf, 'color', 'w', 'units', 'normalized', 'position', [0 0 .9 .9]);
nsubs = length(cfg.subjIDs);



if strcmp(cfg.plotlevel, 'PFX')
    for ippant = 1:nsubs
        clf;
        
        psubj= cfg.subjIDs{ippant}(1:2); % print ppid.
        % both this and the next use the same figure function:
        
        usegaitfields = {'gc_', 'doubgc_'};
        usetypes = {'_allstatnry', '_allwlking'};
        
        %% Palamedes parameter grid, used across all plots:
        % %Parameter grid defining parameter space through which to perform a
        % %brute-force search for values to be used as initial guesses in iterative
        % %parameter search.
        
        %contrast values across exp (perppant):
        StimList= dataIN(ippant,1).gc_ContrVals;
        
        PF = @PAL_Logistic;
        searchGrid.alpha = StimList(1):.001:StimList(end);
        searchGrid.beta = logspace(0,3,101);
        searchGrid.gamma = 0.0;  %scalar here (since fixed) but may be vector
        searchGrid.lambda = 0.02;  %ditto
        
        %Threshold and Slope are free parameters, guess and lapse rate are fixed
        paramsFree = [1 1 0 0];  %1: free parameter, 0: fixed parameter
        % for plots:
        StimLevelsFineGrain=[min(StimList):max(StimList)./1000:max(StimList)];
        %%
        for nGaits_toPlot=1%:2
            
            useg= usegaitfields{nGaits_toPlot};
            legp=[]; % for legend
            
            %% first plot the total stationary, and total walking data.
            
            StimList= dataIN(ippant,1).([useg 'ContrVals']);
            usetypes = {'_allstatnry', '_allwlking'};
            % first a simple bar chart comparison.
            barD=[];
            for iStWlk=1:2
                NumPer = dataIN(ippant,1).([useg 'NumPerContr' usetypes{iStWlk}]);
                TotalPer = dataIN(ippant,1).([useg 'TotalPerContr' usetypes{iStWlk}]);
                RTs = dataIN(ippant,1).([useg 'RTperContr' usetypes{iStWlk}]);
                % take mean accuracy over all contrast vals.
                mAcc = sum(NumPer) / sum(TotalPer);
                
                barD(iStWlk) = mAcc;
                
                barD_rt(iStWlk,:) = RTs;
            end
            %%
            subplot(221);
            bh1= bar([barD(1), nan]);            hold on
            bh2= bar([ nan, barD(2)]);
            bh1.FaceColor =  usecolsStWlk{1};
            bh2.FaceColor =  usecolsStWlk{2};
            ylabel('Accuracy');
            ylim([0 1]);
            title(psubj);
            set(gca, 'xticklabels', {'standing', 'walking'}, 'fontsize', 15);
            subplot(222);
            bar(barD_rt, 'FaceColor', usecolsStWlk{1}); ylabel('RT (sec)');
            set(gca, 'xticklabels', {'standing', 'walking'}, 'fontsize', 15);
            title('RT by contrast');
            ylim([0 .8])
            %%
            %now for PF fits.
            lg=[];
            
            
            
            for iStWlk=1:2
                
                % first plot mean acc and RT
                
                % now PF fits
                NumPer = dataIN(ippant,1).([useg 'NumPerContr' usetypes{iStWlk}]);
                TotalPer = dataIN(ippant,1).([useg 'TotalPerContr' usetypes{iStWlk}]);
                
                % take mean accuracy over all contrast vals.
                mAcc = sum(NumPer) / sum(TotalPer);
                % perform PF fit.
                %% avoid infinte values.
                zerolist = find(NumPer==0);
                NumPer(zerolist)= .001;
                %
                [paramsValues LL exitflag] = PAL_PFML_Fit(StimList,NumPer, ...
                    TotalPer,searchGrid,paramsFree,PF);
                
                %Create simple plot
                ProportionCorrectObserved=NumPer./TotalPer;
                ProportionCorrectModel = PF(paramsValues,StimLevelsFineGrain);
                
                subplot(2,2,3);
                
                hold on
                plot(StimLevelsFineGrain,ProportionCorrectModel,'-','color',usecolsStWlk{iStWlk},'linewidth',4);
                lg(iStWlk)=plot(StimList,ProportionCorrectObserved,['.'],'color', usecolsStWlk{iStWlk},'markersize',40);
                
                %tidyax
                set(gca, 'fontsize',12);
                set(gca, 'Xtick',StimList);
                
                xlabel('Stimulus Intensity');
                ylabel('proportion correct');
                hold on
                title(psubj);
            end
            legend([lg], {'standing', 'walking'}, 'location', 'NorthWest')
            
            %% next plot the walking data, split by feet.
            %use ylims from prev plot
            usey = get(gca, 'ylim');
            subplot(224);
            %replot walking, with increased transparency for comparison.
            plot(StimLevelsFineGrain,ProportionCorrectModel,'-','color',[usecolsStWlk{iStWlk}, .3],'linewidth',4);
            hold on
            %              plot(StimList,ProportionCorrectObserved,['.'],'color', [usecolsStWlk{iStWlk}],'markersize',40);
            % Over lay L/R data.
            usetypes = {'_LRwlking'};
            lg=[];
            for iLR=1:2 % left / right step leading.
                if nGaits_toPlot==1
                    plotHead = GFX_headY(ippant).gc;
                    pidx= cfg.pidx1;
                    ftnames= {'LR', 'RL', 'all'};
                else
                    plotHead = GFX_headY(ippant).doubgc;
                    pidx= cfg.pidx2;
                    ftnames= {'LRL', 'RLR', 'all'};
                end
                NumPer = dataIN(ippant,iLR).([useg 'NumPerContr_LRwlking']);
                TotalPer = dataIN(ippant,iLR).([useg 'TotalPerContr_LRwlking']);
                
                %%
                zerolist = find(NumPer==0);
                NumPer(zerolist)= .001;
                %
                [paramsValues LL exitflag] = PAL_PFML_Fit(StimList,NumPer, ...
                    TotalPer,searchGrid,paramsFree,PF);
                
                threshEst(iLR) = paramsValues(1);
                slopeEst(iLR) = paramsValues(2);
                
                %Create simple plot
                ProportionCorrectObserved=NumPer./TotalPer;
                ProportionCorrectModel = PF(paramsValues,StimLevelsFineGrain);
                
                subplot(2,2,4);
                hold on
                plot(StimLevelsFineGrain,ProportionCorrectModel,'-','color',usecolsLR{iLR},'linewidth',4);
                lg(iLR)=plot(StimList,ProportionCorrectObserved,['.'],'color', usecolsLR{iLR},'markersize',40);
                
                
                
            end % i LR
            %%
            ylim(usey);
            legend([lg], ftnames, 'location', 'NorthWest')
            
        end % nGaits.
        %%
        cd([cfg.datadir filesep  'Figures' filesep  cfg.type ' Accuracy'])
        print('-dpng', [psubj ' accuracy and rt summary']);
        
    end % ppant
end
if strcmp(cfg.plotlevel, 'GFX')
    clf;
    
    psubj= ['Group N=' num2str(nsubs)];
    % both this and the next use the same figure function:
    
    usegaitfields = {'gc_', 'doubgc_'};
    usetypes = {'_allstatnry', '_allwlking'};
    
    %% Palamedes parameter grid, used across all plots:
    % %Parameter grid defining parameter space through which to perform a
    % %brute-force search for values to be used as initial guesses in iterative
    % %parameter search.
    
    PF = @PAL_Logistic;
   
    %all are free parameters, guess and lapse rate are fixed
    searchGrid.alpha = [-1:.01:1];    %structure defining grid to
    searchGrid.beta = 10.^[-1:.01:2]; %search for initial values
    searchGrid.gamma = [0:.01:.06];
    searchGrid.lambda = [0:.01:.06];
    paramsFree = [1 1 1 1];  %1: free parameter, 0: fixed parameter
    
    %extract data across ppants (for-loop once).
    acc_StWlk = zeros(nsubs,2); %mean acc
    rt_StWlk  =zeros(nsubs,2,7); % rt per contrast level.
    fitsObserved_StWlk = zeros(nsubs,2,7); %acc per contrast level.
    fitsModel_StWlk = [];%Unforutnately, different fit (lengths) per subj, due to different stim spacing.
    
    fitsObserved_Wlk_LR = zeros(nsubs,2,7); %fits split by leading foot.
    fitsModel_Wlk_LR = []; 
    [GFX_params_StWlk,GFX_params_Wlk_LR]=deal(zeros(nsubs,2,2)); % 2 parmas(threshold and slopes).
    GFX_contrastLevels = zeros(nsubs,7);
    % which plot type? only 1 gait supported for now.
    for nGaits_toPlot=1%:2
        
        useg= usegaitfields{nGaits_toPlot};
        for ippant = 1:nsubs
            %extract standing and walking data:
            %contrast values across exp (perppant):
            StimList= dataIN(ippant,1).gc_ContrVals;
            %switching StimList to be uniform for ppants (around +- around threshold);
            StimList= -3:1:3;
            
            
            GFX_contrastLevels(ippant,:)= StimList;
            
            
            usetypes = {'_allstatnry', '_allwlking'};
            for iStWlk=1:2
                NumPer = dataIN(ippant,1).([useg 'NumPerContr' usetypes{iStWlk}]);
                TotalPer = dataIN(ippant,1).([useg 'TotalPerContr' usetypes{iStWlk}]);
                RTs = dataIN(ippant,1).([useg 'RTperContr' usetypes{iStWlk}]);
                % take mean accuracy over all contrast vals.
                mAcc = sum(NumPer) / sum(TotalPer);
                
                acc_StWlk(ippant,iStWlk) = mAcc;
                
                rt_StWlk(ippant, iStWlk,:) = RTs;
                
                % also fit per ppant, store fit:
%                 NumPer(NumPer==0)= .001;
                %
                [paramsValues LL exitflag] = PAL_PFML_Fit(StimList,NumPer, ...
                    TotalPer,searchGrid,paramsFree,PF, 'lapseLimits', [0 1],'guessLimits',...
                    [0 1]);
                
                
                fitsObserved_StWlk(ippant,iStWlk,:) = NumPer./ TotalPer;
%                 fitsModel_StWlk(ippant).d(iStWlk,:) = PF(paramsValues,StimLevelsFineGrain);
                
                GFX_params_StWlk(ippant, iStWlk,:) = paramsValues(1:2);
            end
            % extract observed and fit per L/R ft.
            usetypes = {'_LRwlking'};
            
            for iLR=1:2 % left / right step leading.
                
                NumPer = dataIN(ippant,iLR).([useg 'NumPerContr_LRwlking']);
                TotalPer = dataIN(ippant,iLR).([useg 'TotalPerContr_LRwlking']);
                NumPer(NumPer==0)= .001;
                [paramsValues LL exitflag] = PAL_PFML_Fit(StimList,NumPer, ...
                    TotalPer,searchGrid,paramsFree,PF);
                
                
                fitsObserved_Wlk_LR(ippant,iLR,:) = NumPer./TotalPer;
%                 fitsModel_Wlk_LR(ippant,iLR,:) =  PF(paramsValues,StimLevelsFineGrain);
                
                GFX_params_Wlk_LR(ippant,iLR,:) = paramsValues(1:2);
            end % LR
            
        end % ippant
        
        %% show contrast levels per ppant.
%         figure(9);
%         plot(1:size(GFX_contrastLevels,1), GFX_contrastLevels', 'o', 'color','b')
%         ylabel('contrast'); xlabel('ppant');
        %% % continue with plots:
        %% First Accuracy %%%%%%%%%%%%%%%%%%%%%%%%%%%%
        legp=[]; % for legend
        
        mBar = squeeze(nanmean(acc_StWlk,1));
        errBar = CousineauSEM(acc_StWlk);
        %%
        subplot(221);
        bh1= bar([mBar(1), nan], 'FaceAlpha', .5, 'FaceColor', usecolsStWlk{1} );            hold on
        bh2= bar([ nan, mBar(2)], 'FaceAlpha', .5, 'FaceColor', usecolsStWlk{2});
        
        
        errorbar(1:2, mBar, errBar, 'k','linestyle', 'none', 'linew', 2);
        %include individual dpoints:
        scD= ones(1,nsubs);
        %add jitter to display ind subj points:
        jit = (rand(1,100) - .5)./8;
        plot(1+jit(1:nsubs), acc_StWlk(:,1),'.', 'color',  [usecolsStWlk{1}, .9], 'markersize', 20)
        plot(2+jit(1:nsubs), acc_StWlk(:,2),'.', 'color',  [usecolsStWlk{2}, .9], 'markersize', 20)
        % connect per subj.
        for ippant= 1:nsubs
            plot([1+jit(ippant) 2+jit(ippant)], [acc_StWlk(ippant,1), acc_StWlk(ippant,2)], 'color', [.8 .8 .8], 'linew', 1)
        end
        
        %tidy axes
        ylabel('Accuracy');
        title(psubj);
        set(gca, 'xticklabels', {'standing', 'walking'}, 'fontsize', 20);
        %% add sig:
         %add text with relv stats:
        [~,accp, ~, stats] = ttest(acc_StWlk(:,1),  acc_StWlk(:,2));
        
        textmsg = {['\itt\rm(' num2str(stats.df) ')=' sprintf('%.2f',stats.tstat) ','];['\itp \rm= ' sprintf('%.3f', accp) ]};
        text(2.5,.2,textmsg, 'fontsize', 15);
        
        xlim([0 3]);
        %% now RTs %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
        subplot(222);
        mBar =  squeeze(nanmean(rt_StWlk,1));
        st1 = CousineauSEM(squeeze(rt_StWlk(:,1,:)));
        st2 = CousineauSEM(squeeze(rt_StWlk(:,2,:)));
        errBar = [st1;st2];
        %%
        
        bh1= bar([mBar(1,:); nan(1,7)], 'FaceAlpha', .5, 'FaceColor', usecolsStWlk{1});            hold on
        bh2= bar([ nan(1,7); mBar(2,:)], 'FaceAlpha', .5, 'FaceColor', usecolsStWlk{2});
        legend([bh1(1), bh2(2)], {'standing', 'walking'}, 'autoupdate', 'off')
        %%
        errorbar_groupedfit( mBar, errBar);
        set(gca, 'xticklabels', {'increasing contrast', 'increasing contrast'}, 'fontsize', 20);
        title({['RT by contrast'];['low->high']});
        ylabel('Reaction time [secs]');
        %% Now PF FITS %%%%%%%%%%%%%%%%%%%%%%%%%%%
        %now for PF fits.
        lg=[];
        
        %for group level plots, change x axis to 1:7
        StimList = 1:7;        
        StimLevelsFineGrain=[min(StimList):max(StimList)./1000:max(StimList)];
        for iStWlk=1:2
            
            subplot(2,2,3);
            % Plot the group fits, but do stats at individual level
            % (compare paramValues)
%             
%             ProportionCorrectModel =squeeze(nanmean(fitsModel_StWlk(:,iStWlk,:),1));
%             errModel = CousineauSEM(squeeze(fitsModel_StWlk(:,iStWlk,:)));
%             shadedErrorBar(StimLevelsFineGrain, ProportionCorrectModel, errModel, {'color', usecolsStWlk{iStWlk}});
%             
            
            ProportionCorrectObserved = squeeze(nanmean(fitsObserved_StWlk(:, iStWlk,:),1));
            hold on
            
            lg(iStWlk)=plot(StimList,ProportionCorrectObserved,['.'],'color', usecolsStWlk{iStWlk},'markersize',20);
            errObs = CousineauSEM(squeeze(fitsObserved_StWlk(:,iStWlk,:)));
            errorbar(StimList, ProportionCorrectObserved, errObs, 'k', 'linestyle', 'none');
            
            %%add group fit:
%             [paramsValues LL exitflag] = PAL_PFML_Fit(StimList,round(ProportionCorrectObserved.*100)', ...
%                 repmat(100,1, length(ProportionCorrectObserved)),searchGrid,paramsFree,PF);
paramsValues = PAL_PFML_Fit(StimList,ProportionCorrectObserved', ...
                ones(1, length(ProportionCorrectObserved)),searchGrid,paramsFree,PF);

            groupFit = PF(paramsValues,StimLevelsFineGrain);
            
            plot(StimLevelsFineGrain, groupFit, '-', 'color',usecolsStWlk{iStWlk} , 'linew', 2);
            %tidyax
            set(gca, 'fontsize',20);
            set(gca, 'Xtick',StimList, 'xticklabel', split(num2str(StimList-4))');
            
            xlabel('contrast increment');
            ylabel('proportion correct');
            hold on
            title(psubj);
        end
        %%
        xlim([.5 7.5]);
        legend([lg], {'standing', 'walking'}, 'location', 'NorthWest')
        %add text with relv stats:
        [~,threshsig, ~, thrstats] = ttest(squeeze(GFX_params_StWlk(:,1,1)),  squeeze(GFX_params_StWlk(:,2,1)));
        [~,slopesig, ~, slpstats] = ttest(squeeze(GFX_params_StWlk(:,1,2)),  squeeze(GFX_params_StWlk(:,2,2)));
        
        
        textmsg = {['thresh. \itt\rm(' num2str(thrstats.df) ')=' sprintf('%.2f',thrstats.tstat) ', \itp \rm= ' sprintf('%.3f', threshsig) ];...
            ['slope \itt\rm(' num2str(slpstats.df) ')=' sprintf('%.2f',slpstats.tstat) ', \itp \rm= ' sprintf('%.3f', slopesig) ]};
        text(5,.2,textmsg, 'fontsize', 15);
        
        
        %% next plot the walking data, split by feet.
        %use ylims from prev plot
        usey = get(gca, 'ylim');
        
        subplot(224);
        %replot walking final fit, with increased transparency for comparison.
        %shadedErrorBar(StimLevelsFineGrain, ProportionCorrectModel, errModel, {'color', usecolsStWlk{iStWlk}},1);
        hold on
        %
        lg=[];
        for iLR=1:2 % left / right step leading.
            if nGaits_toPlot==1
                ftnames= {'LR', 'RL'};
            else
                ftnames= {'LRL', 'RLR'};
            end
            
            subplot(2,2,4);
            hold on
%             % plot the average of ppant fits?
%             ProportionCorrectModel =squeeze(nanmean(fitsModel_Wlk_LR(:,iLR,:),1));
%             errModel = CousineauSEM(squeeze(fitsModel_Wlk_LR(:,iLR,:)));
%             %                 shadedErrorBar(StimLevelsFineGrain, ProportionCorrectModel, errModel, {'color', usecolsLR{iLR}},1);
            
            % plot mean per contrast level, with error bars:
            ProportionCorrectObserved = squeeze(nanmean(fitsObserved_Wlk_LR(:, iLR,:),1));
            hold on            
            lg(iLR)=plot(StimList,ProportionCorrectObserved,['.'],'color', usecolsLR{iLR},'markersize',20);
            errObs = CousineauSEM(squeeze(fitsObserved_Wlk_LR(:,iLR,:)));
            errorbar(1:length(errObs), ProportionCorrectObserved, errObs, 'k', 'linestyle', 'none');
            
            %%add group fit:
            [paramsValues LL exitflag] = PAL_PFML_Fit(StimList,ProportionCorrectObserved', ...
                ones(1,length(ProportionCorrectObserved)),searchGrid,paramsFree,PF);
           
            groupFit = PF(paramsValues,StimLevelsFineGrain);
            
            plot(StimLevelsFineGrain, groupFit, '-', 'color',usecolsLR{iLR} , 'linew', 2);
        end % i LR
        %%
                xlim([.5 7.5]);

        set(gca, 'fontsize',20);
         set(gca, 'Xtick',StimList, 'xticklabel', split(num2str(StimList-4))');
            
        
        xlabel('contrast increment');
        ylabel('proportion correct');
        ylim(usey);
        legend([lg], ftnames, 'location', 'NorthWest', 'autoupdate', 'off')
        %add text with relv stats:
        [~,threshsig, ~, thrstats] = ttest(squeeze(fitsObserved_Wlk_LR(:,1,1)),  squeeze(fitsObserved_Wlk_LR(:,2,1)));
        [~,slopesig, ~, slpstats] = ttest(squeeze(fitsObserved_Wlk_LR(:,1,2)),  squeeze(fitsObserved_Wlk_LR(:,2,2)));
        
        
        textmsg = {['thresh. \itt\rm(' num2str(thrstats.df) ')=' sprintf('%.2f',thrstats.tstat) ', \itp \rm= ' sprintf('%.3f', threshsig) ];...
            ['slope \itt\rm(' num2str(slpstats.df) ')=' sprintf('%.2f',slpstats.tstat) ', \itp \rm= ' sprintf('%.3f', slopesig) ]};
        text(5,.2,textmsg, 'fontsize', 15);
        
    end % nGaits.
    %%
    cd([cfg.datadir filesep  'Figures' filesep  cfg.type ' Accuracy'])
    print('-dpng', [psubj ' accuracy and rt summary']);
    
    
    
end% GFX
end