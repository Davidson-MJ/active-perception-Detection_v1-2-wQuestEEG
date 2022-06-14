function  plot_PFfits_gaitcycle(dataIN, cfg)
% helper function to plot the psychometric fits for different
% quantiles of the gait cycle.
% plotjobs_Data_withinGait_PFfits.m

GFX_headY = cfg.HeadData;
figure(1); clf; set(gcf, 'color', 'w', 'units', 'normalized', 'position', [0 0 .9 .9]);
nsubs = length(cfg.subjIDs);


qntlCols = cbrewer('seq', 'Reds', 100);
% qntlCols = cbrewer('div', 'Spectral', 100);
qntlCols(qntlCols>1)=1;

if strcmp(cfg.plotlevel, 'PFX')
    for ippant = 1:nsubs
        clf;
        
        psubj= cfg.subjIDs{ippant}(1:2); % print ppid.
        % both this and the next use the same figure function:
        
        usegaitfields = {'gc', 'doubgc'};
        usetypes = {'_qntlwlking'};
        
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
            lg=[];
            
            
            %% first plot the Head data, with overlay to show quantile division.
            qntlBounds = dataIN(ippant,1).([useg '_qntl_bounds']);
            
            %% plot Head data.
            if nGaits_toPlot==1
                plotHead = GFX_headY(ippant).gc;
                pidx= cfg.pidx1;
                ftnames= {'LR', 'RL', 'all'};
            else
                plotHead = GFX_headY(ippant).doubgc;
                pidx= cfg.pidx2;
                ftnames= {'LRL', 'RLR', 'all'};
            end
            %%
            for iLR=1:3
                subplot(3,3,1 + (3*(iLR-1)));
                % plot Head data
                mP= nanmean(plotHead,1);
                plot(1:size(plotHead,2),mP, ['k-o'])
                yls = get(gca, 'ylim');
                
                % place patch color in BG.
                for iq = 1:length(qntlBounds)-1
                    tidx = qntlBounds(iq):qntlBounds(iq+1);
                    hold on;
                    thisColr= qntlCols(qntlBounds(iq),:);
                    plot(tidx, mP(tidx), 'color',thisColr , 'linew', 3);
                    %add patch ?
                    
                    xv = [tidx(1) tidx(1) tidx(end) tidx(end)];
                    yv = [yls(1) yls(2) yls(2) yls(1)];
                    ph = patch('XData', xv, 'YData', yv, 'FaceColor', thisColr, 'FaceAlpha', .2, 'LineStyle','none');
                    %             ph.FaceColor = thisColr;
                end
                ylim(yls);
                title([ psubj ' ' ftnames{iLR}]);
                %% now plot the psychometric curves for each portion.
                NumPerQ= dataIN(ippant, iLR).([useg '_NumPerContr_qntlwlking']);
                TotalPerQ = dataIN(ippant,iLR).([useg '_TotalPerContr_qntlwlking']);
                
                %store threshold and slope for next plot:
                ppantParams= nan(2, size(NumPerQ,1));
                for iqnt= 1:size(NumPerQ,1)
                    %qntl specific data:
                    NumPer = NumPerQ(iqnt,:);
                    TotalPer = TotalPerQ(iqnt,:);
                    thisColr= qntlCols(qntlBounds(iqnt),:);
                    %%
                    zerolist = find(NumPer==0);
                    NumPer(zerolist)= .001;
                    %
                    [paramsValues LL exitflag] = PAL_PFML_Fit(StimList,NumPer, ...
                        TotalPer,searchGrid,paramsFree,PF);
                    
                    %                 threshEst(iLR) = paramsValues(1);
                    %                 slopeEst(iLR) = paramsValues(2);
                    
                    %Create simple plot
                    ProportionCorrectObserved=NumPer./TotalPer;
                    ProportionCorrectModel = PF(paramsValues,StimLevelsFineGrain);
                    
                    subplot(3,3,2 + (3*(iLR-1)));
                    hold on
                    plot(StimLevelsFineGrain,ProportionCorrectModel,'-','color', thisColr,'linewidth',4);
                    lg(iLR)=plot(StimList,ProportionCorrectObserved,['.'],'color', thisColr,'markersize',40);
                    xlabel('contrast')
                    set(gca, 'color', [.8 .8 .8])
                    
                    ppantParams(:,iqnt) = paramsValues(1:2);
                end % iqnt
                title('fits over the gait');
                set(gca, 'fontsize', 20);
                
                %% add summary bar chart to clarify effect:
         
        %compare thresh, and slope, in each quantile.
        barDataPlot = {ppantParams(1,:), ppantParams(2,:)};
        ytitles = {'threshold values', 'slope parameter'};
        ttitles = {['thresh per quant.'], ['slope per quant.']};
        subplotspot= [5,6];
       
        
        for ithrSlp = 1:2 % 5 and 6 or subplot pos.
          subplot(3,6,subplotspot(ithrSlp) + (6*(iLR-1)));
          cla
        barD = barDataPlot{ithrSlp};
           mBar = nanmean(barD,1);
       bh= bar(1:size(barD,2),mBar,'FaceColor','flat'); %flat allows us to update colours below
       bh.CData = qntlCols(qntlBounds(1:end-1),:);
       hold on;

% adjust ylims to sdrange.
        sdrange = max(mBar) - min(mBar);
        ylim([min(mBar)-3*sdrange max(mBar)+3*sdrange])
       title(ttitles{ithrSlp})
       ylabel(ytitles{ithrSlp})
       xlabel('quantile');
        end % each param to plot.

            end % iLR
            %%
            %              legend([lg], ftnames, 'location', 'NorthWest')
            
        end % nGaits.
        %%
        cd([cfg.datadir filesep  'Figures' filesep  cfg.type ' qntl PsychFits'])
        
        print([psubj ' ' cfg.type ' onset qntl PsychFits'],'-dpng');
        
    end % ppant
end
if strcmp(cfg.plotlevel, 'GFX')
    clf;
    
    psubj= 'GFX';
    % both this and the next use the same figure function:
    
    usegaitfields = {'gc', 'doubgc'};
    usetypes = {'_qntlwlking'};
    %% Palamedes parameter grid, used across all plots:
    % %Parameter grid defining parameter space through which to perform a
    % %brute-force search for values to be used as initial guesses in iterative
    % %parameter search.
    
    %contrast values across exp (perppant):
    %                
    
    PF = @PAL_Logistic;
    searchGrid.beta = logspace(0,3,101);
    searchGrid.gamma = 0.0;  %scalar here (since fixed) but may be vector
    searchGrid.lambda = 0.02;  %ditto
    
    %Threshold and Slope are free parameters, guess and lapse rate are fixed
    paramsFree = [1 1 0 0];  %1: free parameter, 0: fixed parameter
   
    
    %extract data across ppants (for-loop once).
    GFX_headdata= zeros(nsubs, 100);
    fitsObserved_WlkQuantile = zeros(nsubs, 2,3, 7); % contrast levels.
%     fitsModel_WlkQuantile = zeros(nsubs,2, 3,length(StimLevelsFineGrain));
    GFX_params_WlkQuantile=zeros(nsubs,2,3,2); % subs, iLR, quantiles, slope/thresh.
    
    for nGaits_toPlot=1%:2 % 2 not yet supported
        
        useg= usegaitfields{nGaits_toPlot};
        
        qntlBounds = dataIN(1,1).([useg '_qntl_bounds']); % should be the same across ppants.
    
        for ippant=1:nsubs
            GFX_headdata(ippant,:) = GFX_headY(ippant).gc;
             StimList= dataIN(ippant,1).gc_ContrVals;             
             searchGrid.alpha = StimList(1):.001:StimList(end);
             % for plots:
   
             for iLR=1:3 % 3rd dim is combined.
                NumPerQ= dataIN(ippant, iLR).([useg '_NumPerContr_qntlwlking']);
                TotalPerQ = dataIN(ippant,iLR).([useg '_TotalPerContr_qntlwlking']);
                
                for iqnt= 1:size(NumPerQ,1)
                    %qntl specific data:
                    NumPer = NumPerQ(iqnt,:);
                    TotalPer = TotalPerQ(iqnt,:);
                    %% remove any zeros:
                    NumPer(NumPer==0)= .001;
                    [paramsValues LL exitflag] = PAL_PFML_Fit(StimList,NumPer, ...
                        TotalPer,searchGrid,paramsFree,PF);
                    
                    %store observed data and ppant fit:
                    fitsObserved_WlkQuantile(ippant, iLR,iqnt,:)=NumPer./TotalPer;
                    
                    if any(isinf(paramsValues))
                        disp('COULD NOT FIT PFIT to this ppant');
                        paramsValues(1:2)=nan(1,2);
                    end
                    
                GFX_params_WlkQuantile(ippant,iLR,iqnt,:) = paramsValues(1:2);
                    
            
                
                end % iqnt
            end % iLR
        end % ippant.
        
        %% % continue with plots: %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%         ftnames= {'LR', 'RL'};
        ftnames = {'L foot -> R foot', 'R foot -> L foot', 'all'};
        %since contrast levels were linearly spaced, use 1:7 across ppants.
        StimList=1:7;
         StimLevelsFineGrain=[min(StimList):max(StimList)./1000:max(StimList)];
    
        for iLR=1:3
            subplot(3,3,1 + (3*(iLR-1)));
            % plot Head data
            mP= nanmean(GFX_headdata,1);
%             errP = CousineauSEM(GFX_headdata);
            plot(1:size(GFX_headdata,2),mP, ['k-o'])
            yls = get(gca, 'ylim');            
            % place patch color in BG.
            for iq = 1:length(qntlBounds)-1
                tidx = qntlBounds(iq):qntlBounds(iq+1);
                hold on;
                thisColr= qntlCols(qntlBounds(iq),:);
                plot(tidx, mP(tidx), 'color',thisColr , 'linew', 3);
                %add patch ?
                
                xv = [tidx(1) tidx(1) tidx(end) tidx(end)];
                yv = [yls(1) yls(2) yls(2) yls(1)];
                ph = patch('XData', xv, 'YData', yv, 'FaceColor', thisColr, 'FaceAlpha', .2, 'LineStyle','none');
                %             ph.FaceColor = thisColr;
            end
            ylim(yls);
            title([ psubj ' ' ftnames{iLR}]);
            set(gca, 'fontsize', 20, 'ytick', []);
            ylabel('head height')
            xlabel('% step completion')
            %% now plot the psychometric curves for each portion.
          
            for iqnt= 1:size(NumPerQ,1)
            
                subplot(3,3,2 + (3*(iLR-1)));
                hold on;
                   thisColr= qntlCols(qntlBounds(iqnt),:);
               %plot mean observed values per contrast, with errorbarS:
                
                ProportionCorrectObserved = squeeze(nanmean(fitsObserved_WlkQuantile(:, iLR,iqnt,:),1));
               %plot data:
                lg(iLR)=plot(StimList,ProportionCorrectObserved,['.'],'color',thisColr,'markersize',20);                
                %plot error:
                  errObs = CousineauSEM(squeeze(fitsObserved_WlkQuantile(:,iLR,iqnt,:)));                
                  errorbar(1:length(errObs), ProportionCorrectObserved, errObs, 'k', 'linestyle', 'none');
%               
%add group fit:
                [paramsValues LL exitflag] = PAL_PFML_Fit(StimList,round(ProportionCorrectObserved.*100)', ...
                             repmat(100,1, length(ProportionCorrectObserved)),searchGrid,paramsFree,PF);
                 groupFit = PF(paramsValues,StimLevelsFineGrain);
                 
                 plot(StimLevelsFineGrain, groupFit, '-', 'color',thisColr , 'linew', 2);
                                
            end % iqnt
             %add text with relv stats: (need rmANOVA)
        % convert data to table:
        subjs = [1:nsubs]';
        [Fs,Ps]=deal(zeros(2,1));
        
        for iThrSl=1:2 % for slope and threshold
            %make columns of the repeated measures (each quantile).
            measures=  squeeze(GFX_params_WlkQuantile(:, iLR, :,iThrSl));       
             t=splitvars(table(subjs, measures));
             %to make it robust to toggling gait quantiles, extract the
             %autonames:
             %wilkinson notation for a rmmodel.
             rmmodel = ['measures_1-measures_' num2str(length(qntlBounds)-1) '~1'];
        
             WithinDesign = table([1:size(measures,2)]','VariableNames',{'quantiles'});

             mdlfit= fitrm(t, rmmodel, 'WithinDesign', WithinDesign);
             
             %get stats from table:
             rtable = ranova(mdlfit);
             Fs(iThrSl,1) = rtable.F(1);
             Ps(iThrSl,1) = rtable.pValue(1);
        end
             
        textmsg1 = ['thresh. \itF\rm(' num2str(rtable.DF(1)) ',' num2str(rtable.DF(2)) ')=' sprintf('%.2f',Fs(1)) ', \itp\rm=' sprintf('%.2f', Ps(1))];
        textmsg2 = ['slope \itF\rm(' num2str(rtable.DF(1)) ',' num2str(rtable.DF(2)) ')=' sprintf('%.2f',Fs(2)) ', \itp\rm=' sprintf('%.2f', Ps(2))];
        text(4,.2,{textmsg1; textmsg2}, 'fontsize', 10);
        
            xlim([.5 7.5])
            xlabel('contrast')
            title('fits over the gait');
            set(gca, 'Xtick',StimList, 'xticklabel', split(num2str(StimList-4))', 'color', [.8 .8 .8]);
            
        
        xlabel('contrast increment');
        ylabel('proportion correct');
        set(gca, 'fontsize', 20);
        
        
        %% add summary bar chart to clarify effect:
        % grouped bar chart:
        barDataThresh = squeeze(GFX_params_WlkQuantile(:, iLR, :,1));
        barDataSlope = squeeze(GFX_params_WlkQuantile(:, iLR, :,2));   
        
        barDataPlot = {barDataThresh, barDataSlope};
        ytitles = {'threshold values', 'slope parameter'};
        ttitles = {['\rm' textmsg1], ['\rm' textmsg2]};
        subplotspot= [5,6];
        ylimsare=[0,.6; 0,200];
        
        for ithrSlp = 1:2 % 5 and 6 or subplot pos.
          subplot(3,6,subplotspot(ithrSlp) + (6*(iLR-1)));
          cla
        barD = barDataPlot{ithrSlp};
           mBar = nanmean(barD,1);
       bh= bar(1:size(barD,2),mBar,'FaceColor','flat'); %flat allows us to update colours below
       bh.CData = qntlCols(qntlBounds(1:end-1),:);
       hold on;
       stE = CousineauSEM(barD);
       errorbar(1:size(barD,2),mBar, stE,'linestyle', 'none')
%        ylim(ylimsare(ithrSlp,:))
% adjust ylims to sdrange.
        sdrange = max(mBar) - min(mBar);
        ylim([min(mBar)-3*sdrange max(mBar)+3*sdrange])
       title(ttitles{ithrSlp})
       ylabel(ytitles{ithrSlp})
       xlabel('quantile');
        end % each param to plot.

%%
        
        end % iLR
       
    end % nGaits
        cd([cfg.datadir filesep  'Figures' filesep  cfg.type ' qntl PsychFits'])
        
        print([psubj ' ' cfg.type ' onset qntl PsychFits'],'-dpng');
        
end% GFX
end